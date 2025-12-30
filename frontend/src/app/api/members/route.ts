import { NextResponse } from "next/server";
import { cookies } from "next/headers";

export const dynamic = "force-dynamic";

export async function GET(req: Request) {
  const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5093";

  const token = (await cookies()).get("token")?.value;
  if (!token) {
    return NextResponse.json({ message: "token cookie missing" }, { status: 401 });
  }

  const url = new URL(req.url);
  const backendUrl = `${apiBase}/api/members/?${url.searchParams.toString()}`;

  const res = await fetch(backendUrl, {
    method: "GET",
    cache: "no-store",
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: "application/json",
    },
  });

  const body = await res.text().catch(() => "");
  return new NextResponse(body, {
    status: res.status,
    headers: {
      "content-type": res.headers.get("content-type") ?? "application/json",
    },
  });
}
