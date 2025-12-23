// src/app/account/profile/page.tsx
import { redirect } from "next/navigation";
import { headers } from "next/headers";
import AccountProfileFormClient from "./AccountProfileFormClient";

type AccountProfileDto = {
  id: number;
  name: string;
  email: string;
  postalCode?: string | null;
  address?: string | null;
  phone?: string | null;
};

export default async function AccountProfilePage() {
  const h = await headers();
  const cookie = h.get("cookie") ?? "";

  // ★ ここが重要：現在のリクエストから proto/host を復元して絶対URL化
  const proto = h.get("x-forwarded-proto") ?? "http";
  const host =
    h.get("x-forwarded-host") ??
    h.get("host") ??
    "localhost:3000";

  const url = `${proto}://${host}/api/account/me`;

  const res = await fetch(url, {
    method: "GET",
    headers: { cookie },
    cache: "no-store",
  });

  if (!res.ok) {
    redirect("/auth/login");
  }

  const initialValue = (await res.json()) as AccountProfileDto;

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-sm sm:text-base font-semibold text-slate-100">
          プロフィール
        </h2>
        <p className="mt-1 text-xs text-slate-400">
          請求書の宛名や連絡先に使用される情報です。
        </p>
      </div>

      <AccountProfileFormClient initialValue={initialValue} />
    </div>
  );
}
