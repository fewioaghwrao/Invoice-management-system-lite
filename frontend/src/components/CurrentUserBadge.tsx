// src/components/CurrentUserBadge.tsx
"use client";

import { useCurrentUser } from "@/hooks/useCurrentUser";

export default function CurrentUserBadge() {
  const user = useCurrentUser();

  if (!user) return null;

  return (
    <div className="flex items-center gap-2 text-sm text-gray-600">
      <span className="px-2 py-1 rounded-full bg-gray-100">
        ロール: {user.role === "Admin" ? "管理者" : "一般会員"}
      </span>
      <span className="text-gray-500">{user.name}</span>
    </div>
  );
}
