export interface BasketItem {
  productId: string;
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Basket {
  userId: string;
  totalAmount: number;
  totalItems: number;
  items: BasketItem[];
}
