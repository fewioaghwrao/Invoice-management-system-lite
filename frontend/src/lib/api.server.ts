// src/lib/api.server.ts
import { cookies } from "next/headers";

const baseUrl =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5093";

function buildUrl(path: string, query?: Record<string, string | number | undefined>) {
  const url = new URL(`${baseUrl}${path}`);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== "") {
        url.searchParams.append(key, String(value));
      }
    }
  }
  return url;
}

export async function apiGetServer<T>(
  path: string,
  query?: Record<string, string | number | undefined>
): Promise<T> {
  const url = buildUrl(path, query);
  const c = await cookies();
  const token = c.get("token")?.value ?? null;

  const res = await fetch(url.toString(), {
    cache: "no-store",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    console.error("API error response:", res.status, text);
    throw new Error(`API error: ${res.status}`);
  }

  return res.json() as Promise<T>;
}
