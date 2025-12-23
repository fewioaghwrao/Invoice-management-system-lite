// src/app/api/auth/logout/route.ts
import { NextResponse } from "next/server";

export async function POST() {
  const res = NextResponse.json({ ok: true });

  // ★あなたの middleware が見ている cookie を消す
  res.cookies.set("isLoggedIn", "", { path: "/", maxAge: 0 });
  res.cookies.set("role", "", { path: "/", maxAge: 0 });

  // もし memberId 等の cookie を使ってるなら、同様に消す
  // res.cookies.set("memberId", "", { path: "/", maxAge: 0 });

  return res;
}
