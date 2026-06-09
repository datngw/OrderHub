export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface Order {
  id: string;
  userId: string;
  status: string; // "Pending" | "Confirmed" | "Shipped" | "Delivered" | "Cancelled"
  totalAmount: number;
  email: string;
  fullName: string;
  phone: string;
  province: string;
  district: string;
  ward: string;
  streetAddress: string;
  note: string;
  items: OrderItem[];
  createdAt: string;
  updatedAt: string | null;
}

export interface OrderListParams {
  page?: number;
  pageSize?: number;
  status?: string; // "Pending" | "Confirmed" | "Shipped" | "Delivered" | "Cancelled"
  search?: string;
  fromDate?: string;
  toDate?: string;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
}

export interface CreateOrderRequest {
  note?: string;
  email: string;
  fullName: string;
  phone: string;
  province: string;
  district: string;
  ward: string;
  streetAddress: string;
}

export interface UpdateOrderStatusRequest {
  status: string; // "Pending" | "Confirmed" | "Shipped" | "Delivered" | "Cancelled"
}
