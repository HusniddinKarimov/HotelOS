#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Runs the eight assessment test scenarios (TS-01..TS-08) against a running
# HotelOS instance. Start the system first with ./run.sh, then run this.
#
# NOTE on room numbers: the brief authorises a simplified 10-room build
# (rooms 101-105 and 201-205, two floors). Scenarios written for the full
# 120-room hotel that name rooms 301 / 115 are demonstrated here against the
# equivalent valid rooms 201 / 105.
# ---------------------------------------------------------------------------
set -uo pipefail
R=http://localhost:5001   # reception
H=http://localhost:5002   # housekeeping
S=http://localhost:5003   # room service
M=http://localhost:5004   # maintenance
j() { python3 -m json.tool 2>/dev/null || cat; }
post() { curl -s -X POST "$1" -H 'Content-Type: application/json' -d "$2"; }

echo "===== TS-01: check in double, floor-3 preference ====="
post $R/checkin '{"guestName":"Alice Smith","roomType":"double","nights":3,"floorPreference":3,"cardNumber":"4111111111111234"}' | j

echo "===== TS-06: two simultaneous double check-ins (no double-booking) ====="
post $R/checkin '{"guestName":"Bob Jones","roomType":"double","nights":1}' &
post $R/checkin '{"guestName":"Carol Lee","roomType":"double","nights":1}' &
wait; echo

echo "===== Occupy a suite (-> 204) and order room service ====="
post $R/checkin '{"guestName":"Dana White","roomType":"suite","nights":2}' | j
post $S/orders '{"roomNumber":204,"items":[{"name":"Coffee","quantity":2,"unitPrice":3.5},{"name":"Club Sandwich","quantity":1,"unitPrice":9.0}]}' | j

echo "===== TS-02: checkout 204 (bill = room + room service) ====="
sleep 1; post $R/checkout '{"roomNumber":204}' | j

echo "===== TS-03: housekeeping cleans 204 (Dirty -> BeingCleaned -> Clean) ====="
curl -s $H/queue | j
post $H/clean/start '{"roomNumber":204}' | j
post $H/clean/done  '{"roomNumber":204}' | j

echo "===== TS-04: order on 201 progresses through all states ====="
OID=$(post $S/orders '{"roomNumber":201,"items":[{"name":"Coffee","quantity":2,"unitPrice":3.5},{"name":"Sandwich","quantity":1,"unitPrice":7.0}]}' | python3 -c "import sys,json;print(json.load(sys.stdin)['orderId'])")
for i in 1 2 3; do post $S/orders/$OID/advance ''; echo; done

echo "===== TS-05: Critical maintenance on 105 (assigned to a technician) ====="
post $M/issues '{"roomNumber":105,"description":"Broken shower","urgency":"Critical"}' | j

echo "===== TS-07: exhaust accessible rooms, then request another ====="
post $R/checkin '{"guestName":"Guest A","roomType":"accessible","nights":1}' >/dev/null
post $R/checkin '{"guestName":"Guest B","roomType":"accessible","nights":1}' >/dev/null
post $R/checkin '{"guestName":"Guest C","roomType":"accessible","nights":1}' | j

echo "===== TS-08: invalid input is rejected safely (no crash) ====="
echo "checkout 999:"; post $R/checkout '{"roomNumber":999}' | j
echo "bad room type:"; post $R/checkin '{"guestName":"X Y","roomType":"penthouse","nights":1}' | j
echo "bad name:";      post $R/checkin '{"guestName":"123","roomType":"double","nights":1}' | j

echo "===== DONE ====="
