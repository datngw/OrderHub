export interface TopProductRevenue {
  productId: string;
  productName: string;
  totalQuantity: number;
  totalRevenue: number;
}

export interface RevenueByDay {
  date: string;
  orderCount: number;
  totalRevenue: number;
}

export interface ReportParams {
  from?: string;
  to?: string;
  top?: number;
}
