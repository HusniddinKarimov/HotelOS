# HotelOS — Real-Time Hotel Management System

A microservices-based hotel operations platform for the fictional **GrandStay Hotel**.
Four independent services communicate **only** through a custom WebSocket message
broker, and a browser dashboard receives live updates over WebSocket.

Built for **Unit 4: Programming** (Pearson BTEC HN in Digital Technologies).

---

## 1. Architecture

```
                         ┌─────────────────────────┐
                         │   Operations Dashboard   │  :5005  (browser + WebSocket)
                         │   (subscribes to "*")    │
                         └────────────▲─────────────┘
                                      │ events
   ┌──────────────┐   ┌──────────────┴──────────────┐   ┌──────────────┐
   │  Reception   │   │      Message Broker          │   │ Housekeeping │
   │    :5001     │◄─►│   ws://localhost:5000/broker  │◄─►│    :5002     │
   └──────────────┘   │   topic-based publish/sub    │   └──────────────┘
   ┌──────────────┐   │                              │   ┌──────────────┐
   │ Room Service │◄─►│                              │◄─►│ Maintenance  │
   │    :5003     │   └──────────────────────────────┘   │    :5004     │
   └──────────────┘                                       └──────────────┘
```

Services never call each other directly. Reception publishes a `room_vacated`
event; Housekeeping receives it without Reception knowing who is listening.

| Project | Port | Responsibility |
|---|---|---|
| `HotelOS.Broker` | 5000 | Topic-based publish/subscribe message broker (WebSocket) |
| `HotelOS.Reception` | 5001 | Room assignment algorithm, billing, room/guest inventory |
| `HotelOS.Housekeeping` | 5002 | Cleaning queue, room status transitions |
| `HotelOS.RoomService` | 5003 | Order queue + order state machine |
| `HotelOS.Maintenance` | 5004 | Priority queue (binary heap) + technician dispatch |
| `HotelOS.Dashboard` | 5005 | Live operations dashboard (auth + WebSocket push) |
| `HotelOS.Contracts` | — | Shared models, events, broker client (no service references another) |

---

## 2. Dependencies

- **.NET SDK 10.0** or later — <https://dotnet.microsoft.com/download>
- A modern web browser (for the dashboard)
- No paid licences, no external services (no RabbitMQ/Redis install required — the
  broker is built into the solution).

Verify your install:

```bash
dotnet --version    # should print 10.x
```

---

## 3. Run it (one command)

**macOS / Linux:**

```bash
./run.sh
```

**Windows (PowerShell):**

```powershell
./run.ps1
```

This starts the broker, all four services and the dashboard. Then open:

> **http://localhost:5005** — access token: **`grandstay2026`**

Press **Ctrl+C** (macOS/Linux) to stop everything.

### Running a single service manually

```bash
dotnet run --project src/HotelOS.Broker        # start this first
dotnet run --project src/HotelOS.Reception
dotnet run --project src/HotelOS.Housekeeping
dotnet run --project src/HotelOS.RoomService
dotnet run --project src/HotelOS.Maintenance
dotnet run --project src/HotelOS.Dashboard
```

Services auto-reconnect to the broker, so start order does not matter.

---

## 4. Replaying the assessment test scenarios

With the system running, in a second terminal:

```bash
./test-scenarios.sh
```

> **Room-number note:** the brief authorises a simplified **10-room** build
> (rooms **101–105** and **201–205**, two floors). Scenarios written for the full
> 120-room hotel that reference rooms **301 / 115** are demonstrated against the
> equivalent valid rooms **201 / 105**. TS-02/03 use room **204**, which exists.

| ID | Scenario | Result |
|----|----------|--------|
| TS-01 | Check in double, floor-3 preference | Assigns cleanest double on any floor (floor 3 absent → fallback) → room 103 |
| TS-02 | Checkout 204 | Bill = room×nights + room-service charges; room → Dirty; `room_vacated` published; Housekeeping queues it |
| TS-03 | Housekeeper cleans 204 | Dirty → BeingCleaned → Clean; dashboard updates live; room re-assignable |
| TS-04 | Order 2 coffees + sandwich | Received → Preparing → OutForDelivery → Delivered; charge posted to bill |
| TS-05 | Critical maintenance | Enters queue at front; next free technician assigned |
| TS-06 | Two simultaneous same-type check-ins | Different rooms assigned; no double-booking (lock-guarded) |
| TS-07 | All rooms of a type occupied | Clear "no rooms available" + alternatives + waitlist; no crash |
| TS-08 | Invalid room number / input | Safe `400` validation error; system stays stable |

---

## 5. Broker event catalogue

| Event (topic) | Publisher | Subscriber(s) | Payload |
|---|---|---|---|
| `reception.room_assigned` | Reception | Dashboard | `{ roomNumber, guestId, guestName, type, floor }` |
| `reception.room_vacated` | Reception | Housekeeping, Dashboard | `{ roomNumber }` |
| `reception.checked_out` | Reception | (Dashboard) | `{ roomNumber, guestName, total }` |
| `housekeeping.status_changed` | Housekeeping | Reception, Dashboard | `{ roomNumber, status }` |
| `roomservice.order_update` | Room Service | Dashboard | `{ orderId, roomNumber, summary, status }` |
| `roomservice.charge` | Room Service | Reception | `{ roomNumber, description, amount }` |
| `maintenance.issue_update` | Maintenance | Reception, Dashboard | `{ issueId, roomNumber, description, urgency, status, technician }` |

Payloads deliberately exclude sensitive data (card numbers are never published).

---

## 6. Data structures (and why)

| Structure | Where | Why |
|---|---|---|
| `List<Room>` | Reception inventory | Small, fixed set; simple iteration for the assignment filter/sort |
| `Dictionary<string,Guest>` | Reception guest records | O(1) lookup by guest id during checkout/charging |
| `Queue<int>` | Housekeeping cleaning board | Rooms cleaned in the order they were vacated (FIFO) |
| `Queue<string>` + `Dictionary<string,Order>` | Room Service | FIFO pipeline + O(1) lookup by order id |
| Binary min-heap (`MaintenancePriorityQueue`) | Maintenance | O(log n) ranking by (urgency, submission order) |

---

## 7. Security

- **Input validation** — every external value is validated in
  `HotelOS.Contracts/Common/Validation.cs` (room numbers, names, nights,
  quantities, free text) before any processing.
- **Authentication** — the dashboard requires a token before any data is sent
  (`/ws?token=…`, checked against `DASHBOARD_TOKEN`, default `grandstay2026`).
- **Data exposure** — broker/WebSocket payloads carry only display-safe fields;
  card numbers stay inside Reception and are shown only as `**** **** **** 1234`.
- **Error handling** — `UseSafeErrors()` middleware catches every exception and
  returns a safe message; raw stack traces are never sent to clients.

Configuration via environment variables (12-factor): `BROKER_URL`, `DASHBOARD_TOKEN`.

---

## 8. Project layout

```
HotelOS/
├── HotelOS.slnx
├── run.sh / run.ps1            # one-command launchers
├── test-scenarios.sh           # replays TS-01..TS-08
└── src/
    ├── HotelOS.Contracts/      # models, events, BrokerClient, validation
    ├── HotelOS.Broker/
    ├── HotelOS.Reception/      # Domain/ : RoomAssignmentService, BillingService, HotelState
    ├── HotelOS.Housekeeping/   # Domain/ : CleaningBoard
    ├── HotelOS.RoomService/    # Domain/ : OrderBoard
    ├── HotelOS.Maintenance/    # Domain/ : MaintenancePriorityQueue, MaintenanceCoordinator
    └── HotelOS.Dashboard/      # Domain/ + wwwroot/index.html
```

---

## 9. Git history

```
<run `git log --oneline` and paste the output here before submitting>
```
