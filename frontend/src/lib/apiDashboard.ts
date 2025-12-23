// src/lib/apiDashboard.ts
import { apiGetClient } from "./api.client";
import type { DashboardSummary } from "./types";

export function getDashboardSummary() {
  return apiGetClient<DashboardSummary>("/admin/summary");
}

