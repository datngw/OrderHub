"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import { useRouter } from "next/navigation";
import type { UserInfo } from "@/types/auth";
import { loginApi, logoutApi, registerApi } from "@/features/auth/api";
import { ROUTES } from "@/lib/constants";

const USER_STORAGE_KEY = "orderhub_user";

interface AuthContextValue {
  user: UserInfo | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  isAdmin: boolean;
  login: (email: string, password: string) => Promise<UserInfo>;
  register: (data: {
    email: string;
    password: string;
    fullName: string;
    phone: string;
  }) => Promise<UserInfo>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function getUserFromStorage(): UserInfo | null {
  if (typeof window === "undefined") return null;
  try {
    const stored = localStorage.getItem(USER_STORAGE_KEY);
    return stored ? JSON.parse(stored) : null;
  } catch {
    return null;
  }
}

function setUserStorage(user: UserInfo | null) {
  if (typeof window === "undefined") return;
  if (user) {
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
  } else {
    localStorage.removeItem(USER_STORAGE_KEY);
  }
}

async function tryRestoreSession(): Promise<UserInfo | null> {
  try {
    const response = await fetch("/api/auth/refresh", { method: "POST" });
    if (!response.ok) return null;

    const data = await response.json();
    return data.user ?? null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const router = useRouter();
  const [user, setUser] = useState<UserInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function initializeAuth() {
      const storedUser = getUserFromStorage();

      if (storedUser) {
        if (!cancelled) {
          setUser(storedUser);
          setIsLoading(false);
        }

        // Validate session in background to make sure tokens haven't expired
        const restoredUser = await tryRestoreSession();
        if (!cancelled) {
          if (restoredUser) {
            setUser(restoredUser);
            setUserStorage(restoredUser);
          } else {
            // Session expired, clear auth state
            setUser(null);
            setUserStorage(null);
          }
        }
        return;
      }

      const restoredUser = await tryRestoreSession();

      if (!cancelled) {
        if (restoredUser) {
          setUser(restoredUser);
          setUserStorage(restoredUser);
        }
        setIsLoading(false);
      }
    }

    initializeAuth();

    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(
    async (email: string, password: string): Promise<UserInfo> => {
      const result = await loginApi(email, password);
      setUserStorage(result.user);
      setUser(result.user);
      router.push(
        result.user.role === "Admin" ? ROUTES.adminDashboard : ROUTES.home
      );
      return result.user;
    },
    [router]
  );

  const register = useCallback(
    async (data: {
      email: string;
      password: string;
      fullName: string;
      phone: string;
    }): Promise<UserInfo> => {
      const result = await registerApi(data);
      setUserStorage(result.user);
      setUser(result.user);
      router.push(
        result.user.role === "Admin" ? ROUTES.adminDashboard : ROUTES.home
      );
      return result.user;
    },
    [router]
  );

  const logout = useCallback(async () => {
    try {
      await logoutApi("");
    } catch {
      // Continue logout even if API call fails
    }
    setUserStorage(null);
    setUser(null);
    router.push(ROUTES.login);
  }, [router]);

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: !!user,
        isAdmin: user?.role === "Admin",
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
