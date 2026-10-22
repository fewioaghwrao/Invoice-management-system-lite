import { render, screen } from "@testing-library/react";
import PaymentNewPage from "../../../../src/app/payments/new/page";

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

jest.mock("../../../../src/app/payments/new/PaymentNewClient", () => {
  return function MockPaymentNewClient() {
    return <div>PaymentNewClient</div>;
  };
});

describe("PaymentNewPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("見出し・説明文・パンくずを表示する", () => {
    render(<PaymentNewPage />);

    expect(screen.getByRole("heading", { name: "入金登録（手動）" })).toBeInTheDocument();
    expect(
      screen.getByText(
        "入金日・金額・入金名義を入力して登録します。登録後は請求書への割当画面へ進みます。"
      )
    ).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "ダッシュボード" })).toHaveAttribute(
      "href", "/dashboards/admin"
    );
    expect(screen.getByRole("link", { name: "入金一覧" })).toHaveAttribute(
      "href", "/payments"
    );
    expect(screen.getByText("新規登録")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "← 入金一覧へ" })).toHaveAttribute(
      "href", "/payments"
    );
  });

  it("PaymentNewClient を表示する", () => {
    render(<PaymentNewPage />);
    expect(screen.getByText("PaymentNewClient")).toBeInTheDocument();
  });
});
