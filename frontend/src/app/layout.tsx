import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import Link from "next/link";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Invoice & Payment Status Dashboard (Lite)",
  description:
    "A lightweight demo dashboard for invoice and payment status management.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="ja" className={`${geistSans.variable} ${geistMono.variable}`}>
      <body className="min-h-screen bg-slate-950 text-slate-50">
        {/* レイアウト全体を縦Flexにして、フッターを最下部へ */}
        <div className="min-h-screen flex flex-col">
          <div className="flex-1">{children}</div>

          {/* 共通フッター（規約リンク） */}
          <footer className="border-t border-slate-800 bg-slate-950/80 backdrop-blur">
            <div className="mx-auto max-w-6xl px-4 sm:px-6 py-4">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <p className="text-[11px] text-slate-400">
                  ※ 本アプリはポートフォリオ目的のデモアプリです。実在する個人情報や機密情報の入力は推奨しません。
                </p>

                <div className="flex items-center gap-3 text-[11px] text-slate-400">
                  <Link href="/terms" className="hover:text-sky-300">
                    利用規約
                  </Link>
                  <span className="text-slate-600">|</span>
                  <Link href="/privacy" className="hover:text-sky-300">
                    プライバシーポリシー
                  </Link>
                </div>
              </div>
            </div>
          </footer>
        </div>
      </body>
    </html>
  );
}
