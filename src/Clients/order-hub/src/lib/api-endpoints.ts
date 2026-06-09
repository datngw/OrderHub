export const API_ENDPOINTS = {
  auth: {
    login: "/api/v1/auth/login",
    register: "/api/v1/auth/register",
    refresh: "/api/v1/auth/refresh",
    forgotPassword: "/api/v1/auth/forgot-password",
    resetPassword: "/api/v1/auth/reset-password",
    changePassword: "/api/v1/auth/change-password",
    logout: "/api/v1/auth/logout",
  },
  products: {
    list: "/api/v1/products",
    adminList: "/api/v1/products/admin",
    detail: (id: string) => `/api/v1/products/${id}`,
    create: "/api/v1/products",
    update: (id: string) => `/api/v1/products/${id}`,
    delete: (id: string) => `/api/v1/products/${id}`,
  },
  orders: {
    list: "/api/v1/orders",
    myOrders: "/api/v1/orders/me",
    detail: (id: string) => `/api/v1/orders/${id}`,
    create: "/api/v2/orders", // v2 for order creation
    updateStatus: (id: string) => `/api/v1/orders/${id}/status`,
    cancel: (id: string) => `/api/v1/orders/${id}/cancel`,
    delete: (id: string) => `/api/v1/orders/${id}`,
  },
  basket: {
    get: "/api/v1/basket",
    addItem: "/api/v1/basket/items",
    updateItem: (productId: string) => `/api/v1/basket/items/${productId}`,
    removeItem: (productId: string) => `/api/v1/basket/items/${productId}`,
    clear: "/api/v1/basket",
  },
  reports: {
    revenueByDay: "/api/v1/admin/reports/revenue-by-day",
    topProducts: "/api/v1/admin/reports/top-products",
  },
} as const;
