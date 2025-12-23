// src/components/MonthlySalesChartClient.tsx
"use client";

import { useState } from "react";    
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import { useRouter, useSearchParams } from "next/navigation";
import { useTransition } from "react";

type MonthlySale = {
  label: string; // "1月" など
  amount: number;
};

type Props = {
  year: number;
  availableYears: number[];
  monthlySales: MonthlySale[];
};

// 追加：四半期フィルタの型
type QuarterFilter = "ALL" | "Q1" | "Q2" | "Q3" | "Q4";

export default function MonthlySalesChartClient({
  year,
  availableYears,
  monthlySales,
}: Props) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  // ★ 追加：四半期フィルタ用 state
  const [quarter, setQuarter] = useState<QuarterFilter>("ALL");

  // 年変更（これは今までどおり）
  const handleYearChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newYear = e.target.value;
    const params = new URLSearchParams(searchParams.toString());

    if (newYear) {
      params.set("year", newYear);
    } else {
      params.delete("year");
    }

    startTransition(() => {
      router.push(`/dashboards/admin?${params.toString()}`);
    });
  };

  // ★ 追加：四半期ボタンクリック
  const handleQuarterChange = (q: QuarterFilter) => {
    setQuarter(q);
  };

  // ★ 四半期で表示範囲フィルタ
  const filteredMonthlySales = monthlySales.filter((_, index) => {
    const month = index + 1; // 1〜12

    switch (quarter) {
      case "Q1":
        return month >= 1 && month <= 3;
      case "Q2":
        return month >= 4 && month <= 6;
      case "Q3":
        return month >= 7 && month <= 9;
      case "Q4":
        return month >= 10 && month <= 12;
      case "ALL":
      default:
        return true;
    }
  });

  // グラフ用データ
  const chartData = filteredMonthlySales.map((m, index) => ({
    // フィルタ後の index だと月がずれるので、本当は month を持たせた方が安全
    name: m.label, // 例: "1月", "2月"
    amount: m.amount,
  }));

  const maxAmount = Math.max(...chartData.map((d) => d.amount || 0), 0);
  const yDomain: [number, number] = [0, maxAmount > 0 ? maxAmount * 1.1 : 1];

  return (
    <div className="space-y-3">
      {/* 上部ヘッダー + 年セレクト + 四半期ボタン */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-sm font-semibold text-slate-100">
            月別売上（請求金額ベース）
          </h2>
          <p className="mt-1 text-[11px] text-slate-400">
            {year}年の請求書金額ベースの売上推移です。四半期ごとに絞り込みできます。
          </p>
        </div>

        <div className="flex items-center gap-3">
          {/* 年セレクト */}
          <div className="flex items-center gap-2 text-[11px]">
            <label htmlFor="year-select" className="text-slate-400">
              表示年
            </label>
            <select
              id="year-select"
              value={year}
              onChange={handleYearChange}
              className="rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-100"
            >
              {availableYears.map((y) => (
                <option key={y} value={y}>
                  {y}年
                </option>
              ))}
            </select>
          </div>

          {/* ★ 四半期フィルタボタン */}
          <div className="flex items-center gap-1 rounded-full bg-slate-900/80 px-1 py-1">
            {(
              [
                { key: "ALL", label: "全期間" },
                { key: "Q1", label: "Q1" },
                { key: "Q2", label: "Q2" },
                { key: "Q3", label: "Q3" },
                { key: "Q4", label: "Q4" },
              ] as const
            ).map((item) => {
              const active = quarter === item.key;
              return (
                <button
                  key={item.key}
                  type="button"
                  onClick={() => handleQuarterChange(item.key)}
                  className={[
                    "rounded-full px-2.5 py-1 text-[10px] font-medium transition",
                    active
                      ? "bg-sky-500 text-slate-950"
                      : "bg-transparent text-slate-300 hover:bg-slate-800",
                  ].join(" ")}
                >
                  {item.label}
                </button>
              );
            })}
          </div>

          {isPending && (
            <span className="text-[10px] text-slate-500">更新中...</span>
          )}
        </div>
      </div>

      {/* グラフ本体 */}
      <div className="mt-2 h-56 w-full min-h-[200px] rounded-xl bg-slate-950/40 px-3 py-3">
        <ResponsiveContainer
          width="100%"
          height="100%"
          minWidth={0}
          minHeight={200}
        >
          <LineChart
            data={chartData}
            margin={{ top: 10, right: 20, left: 0, bottom: 10 }}
          >
            <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" />
            <XAxis
              dataKey="name"
              tick={{ fontSize: 11, fill: "#cbd5f5" }}
              tickLine={false}
              axisLine={{ stroke: "#475569" }}
            />
            <YAxis
              tick={{ fontSize: 11, fill: "#cbd5f5" }}
              tickLine={false}
              axisLine={{ stroke: "#475569" }}
              width={70}
              tickFormatter={(value: number) =>
                new Intl.NumberFormat("ja-JP", {
                  notation: "compact",
                  maximumFractionDigits: 0,
                }).format(value)
              }
              domain={yDomain}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "#020617",
                borderColor: "#1e293b",
                borderRadius: "0.75rem",
                fontSize: "11px",
              }}
              formatter={(value: any) =>
                new Intl.NumberFormat("ja-JP", {
                  style: "currency",
                  currency: "JPY",
                  maximumFractionDigits: 0,
                }).format(value as number)
              }
              labelFormatter={(label) => `${year}年 ${label}`}
            />
            <Line
              type="monotone"
              dataKey="amount"
              stroke="#38bdf8"
              strokeWidth={2}
              dot={{
                r: 3,
                strokeWidth: 1,
                stroke: "#e0f2fe",
                fill: "#0ea5e9",
              }}
              activeDot={{ r: 4 }}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
