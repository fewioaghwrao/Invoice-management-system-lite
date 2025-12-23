// src/hooks/useCurrentUser.ts
"use client";

import { useEffect, useState } from "react";

export type Role = "Admin" | "Member";

export type CurrentUser = {
  id: number;
  email: string;
  name: string;
  role: Role;
  token?: string;
};

export function useCurrentUser() {
  const [user, setUser] = useState<CurrentUser | null>(null);

  useEffect(() => {
    if (typeof window === "undefined") return;

    const raw = localStorage.getItem("currentUser");
    if (!raw) return;

    try {
      const parsed = JSON.parse(raw) as CurrentUser;
      setUser(parsed);
    } catch {
      // 壊れたデータなら削除しておく
      localStorage.removeItem("currentUser");
    }
  }, []);

  return user;
}
