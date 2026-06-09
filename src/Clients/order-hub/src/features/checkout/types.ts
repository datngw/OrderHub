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
