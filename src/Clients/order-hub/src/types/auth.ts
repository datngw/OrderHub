export interface ApiErrorResponse {
  // RFC 9457 ProblemDetails fields
  status?: number;
  title?: string;
  detail?: string;
  type?: string;
  instance?: string;
  // Extensions
  traceId?: string;
  timestamp?: string;
  errors?: Record<string, string[]>;
}

export interface UserInfo {
  email: string;
  fullName: string;
  phone: string;
  role: string;
}
