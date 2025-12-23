// src/proxy.ts
import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

const ADMIN_ONLY_PREFIXES = [
  "/dashboards/admin",
  "/invoices",
  "/members",
  "/payments",
  "/sales",
  "/collections", // NOTE: matcherで /collections/:path* を見るので prefixはこれでOK
];

export function proxy(req: NextRequest) {
  const { pathname } = req.nextUrl;

  // ログイン不要のパス
  if (
    pathname.startsWith("/auth/login") ||
    pathname === "/" ||
    pathname.startsWith("/_next") ||
    pathname.startsWith("/api") || // APIも守るならここは外す
    pathname.startsWith("/favicon")
  ) {
    return NextResponse.next();
  }

  const isLoggedIn = req.cookies.get("isLoggedIn")?.value === "true";
  const role = req.cookies.get("role")?.value; // "Admin" or "Member"

  // 未ログイン → /auth/login
  if (!isLoggedIn) {
    return NextResponse.redirect(new URL("/auth/login", req.url));
  }

  // ★ Admin専用パス（一覧で管理）
  const isAdminOnlyPath = ADMIN_ONLY_PREFIXES.some((p) =>
    pathname.startsWith(p)
  );
  if (isAdminOnlyPath && role !== "Admin") {
    if (role === "Member") {
      return NextResponse.redirect(new URL("/dashboards/member", req.url));
    }
    return NextResponse.redirect(new URL("/auth/login", req.url));
  }

  // ★ 会員（顧客）専用：/dashboards/member は Member のみ許可
  if (pathname.startsWith("/dashboards/member")) {
    if (role !== "Member") {
      if (role === "Admin") {
        return NextResponse.redirect(new URL("/dashboards/admin", req.url));
      }
      return NextResponse.redirect(new URL("/auth/login", req.url));
    }
  }

  // ★追加：会員（顧客）専用：/account も Member のみ許可
  if (pathname.startsWith("/account")) {
    if (role !== "Member") {
      if (role === "Admin") {
        return NextResponse.redirect(new URL("/dashboards/admin", req.url));
      }
      return NextResponse.redirect(new URL("/auth/login", req.url));
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    "/dashboards/:path*",
    "/account/:path*", // ★追加
    "/invoices/:path*",
    "/members/:path*",
    "/payments/:path*",
    "/sales/:path*",
    "/collections/:path*",
  ],
};
