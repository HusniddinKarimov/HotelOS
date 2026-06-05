export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface AuthUser { id: string; username: string; email: string; fullName: string; role: string; isActive: boolean }
export interface AuthResponse {
  accessToken: string; accessTokenExpiresAt: string
  refreshToken: string; refreshTokenExpiresAt: string
  user: AuthUser
}

export interface Room {
  id: string; number: number; floor: number; type: string; roomTypeId: number
  nearElevator: boolean; status: string; lastCleanedAt: string; currentGuest: string | null
}

export interface Guest {
  id: string; fullName: string; email: string; phone: string
  nationality?: string | null; passportNumber?: string | null; createdAt: string
}

export interface Reservation {
  id: string; referenceCode: string; guestId: string; guestName: string
  roomTypeId: number; roomType: string; roomId?: string | null; roomNumber?: number | null
  checkInDate: string; checkOutDate: string; floorPreference?: number | null
  proximityPreference?: string | null; status: string; nights: number
}

export interface BillItem { id: string; description: string; type: string; amount: number; quantity: number }
export interface Bill {
  id: string; reservationId: string; status: string; items: BillItem[]
  subtotal: number; discount: number; total: number; paid: number; balance: number
}

export interface HousekeepingTask { id: string; roomId: string; roomNumber: number; status: string; createdAt: string; startedAt?: string | null; completedAt?: string | null }
export interface OrderItem { name: string; quantity: number; unitPrice: number; lineTotal: number }
export interface Order { id: string; orderNumber: string; roomNumber: number; status: string; items: OrderItem[]; total: number; createdAt: string }
export interface MaintenanceRequest { id: string; roomNumber: number; description: string; priority: string; status: string; sequence: number; assignedToUserId?: string | null; assignedToName?: string | null; reportedAt: string; resolvedAt?: string | null }
export interface Notification { id: string; type: string; message: string; isRead: boolean; createdAt: string }

export interface Dashboard {
  totalRooms: number; availableRooms: number; occupiedRooms: number; dirtyRooms: number
  cleaningRooms: number; maintenanceRooms: number; activeGuests: number; activeOrders: number
  openMaintenanceRequests: number; revenue: number
}

export const ROLES = {
  Administrator: 'Administrator', HotelManager: 'HotelManager', Receptionist: 'Receptionist',
  Housekeeping: 'Housekeeping', KitchenStaff: 'KitchenStaff', RoomServiceStaff: 'RoomServiceStaff',
  MaintenanceStaff: 'MaintenanceStaff', User: 'User',
} as const
