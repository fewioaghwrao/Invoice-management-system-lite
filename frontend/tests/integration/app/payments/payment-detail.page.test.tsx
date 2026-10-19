import { render, screen } from "@testing-library/react";
import PaymentDetailPage from "../../../../src/app/payments/[id]/page";

const mockPaymentDetailClient = jest.fn();

jest.mock("next/link", () => {
  return function MockLink({
    href,
    children,
    className,
  }: {
    href: string;
    children: React.ReactNode;
    className?: string;
  }) {
    return <a href={href} className={className}>{children}</a>;
  };
});

jest.mock("../../../../src/app/payments/[id]/PaymentDetailClient", () => {
  return function MockPaymentDetailClient(props: { paymentId: string }) {
    mockPaymentDetailClient(props);
    return <div>PaymentDetailClient</div>;
  };
});

describe("PaymentDetailPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("見出し・説明文・パンくずを表示する", async () => {
    const element = await PaymentDetailPage({
      params: Promise.resolve({ id: "123" }),
    });

    render(element);

    expect(screen.getByRole("heading", { name: "入金詳細（割当）" })).toBeInTheDocument();
    expect(screen.getByText("入金状況を確認し、請求書への割当を管理します。"))
      .toBeInTheDocument();

    expect(screen.getByRole("link", { name: "ダッシュボード" })).toHaveAttribute(
      "href", "/dashboards/admin"
    );
    expect(screen.getByRole("link", { name: "入金一覧" })).toHaveAttribute(
      "href", "/payments"
    );
    expect(screen.getByText("入金詳細")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "← 入金一覧へ" })).toHaveAttribute(
      "href", "/payments"
    );
  });

  it("PaymentDetailClient を表示する", async () => {
    const element = await PaymentDetailPage({
      params: Promise.resolve({ id: "123" }),
    });

    render(element);
    expect(screen.getByText("PaymentDetailClient")).toBeInTheDocument();
  });

  it("params.id を decodeURIComponent して PaymentDetailClient に渡す", async () => {
    const element = await PaymentDetailPage({
      params: Promise.resolve({
        id: encodeURIComponent("PAY/2026 03"),
      }),
    });

    render(element);

    expect(mockPaymentDetailClient).toHaveBeenCalledWith({
      paymentId: "PAY/2026 03",
    });
  });
});
