export const ROUTES = {
  // Auth
  login: "/login",
  register: "/register",
  forgotPassword: "/forgot-password",
  resetPassword: "/reset-password",

  // Admin
  adminDashboard: "/admin/dashboard",
  adminProducts: "/admin/products",
  adminOrders: "/admin/orders",
  adminOrderDetail: (id: string) => `/admin/orders/${id}`,
  adminAccounts: "/admin/accounts",
  adminAccountDetail: (id: string) => `/admin/accounts/${id}`,

  // Shop (Customer)
  home: "/",
  products: "/products",
  productDetail: (slug: string) => `/products/${slug}`,
  cart: "/cart",
  checkout: "/checkout",
  profile: "/profile",
  myOrders: "/my-orders",
  myOrderDetail: (id: string) => `/my-orders/${id}`,
} as const;

export const ROLES = {
  admin: "Admin",
  customer: "Customer",
} as const;

export const ORDER_STATUS = {
  Pending: "Pending",
  Confirmed: "Confirmed",
  Shipped: "Shipped",
  Delivered: "Delivered",
  Cancelled: "Cancelled",
} as const;

export type OrderStatus = (typeof ORDER_STATUS)[keyof typeof ORDER_STATUS];

export const ORDER_STATUS_LABELS: Record<string, string> = {
  [ORDER_STATUS.Pending]: "Pending",
  [ORDER_STATUS.Confirmed]: "Confirmed",
  [ORDER_STATUS.Shipped]: "Shipped",
  [ORDER_STATUS.Delivered]: "Delivered",
  [ORDER_STATUS.Cancelled]: "Cancelled",
};

export const DEFAULT_PAGE_SIZE = 20;
export const MAX_PAGE_SIZE = 100;
