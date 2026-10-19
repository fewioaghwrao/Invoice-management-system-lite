import { render, screen } from "@testing-library/react";
import PaymentsLayout from "@/app/payments/layout";

jest.mock("@/components/CurrentUserBadge", () => {
  return function MockCurrentUserBadge() {
    return <div>CurrentUserBadge</div>;
  };
});

jest.mock("@/components/LogoutButton", () => {
  return function MockLogoutButton() {
    return <button type="button">LogoutButton</button>;
  };
});

jest.mock("@/components/admin/AdminSidebar", () => {
  return function MockAdminSidebar({ activeSection }: { activeSection: string }) {
    return (
      <nav aria-label="管理者メニュー" data-active-section={activeSection}>
        管理者サイドバー
      </nav>
    );
  };
});

describe("PaymentsLayout", () => {
  it("共通ヘッダー・ログイン情報・入金メニュー・ページ本体を表示する", () => {
    render(
      <PaymentsLayout>
        <div>ページ内容</div>
      </PaymentsLayout>
    );

    expect(screen.getByRole("link", { name: "請求・入金管理システム（管理者）" }))
      .toHaveAttribute("href", "/dashboards/admin");
    expect(screen.getByText("CurrentUserBadge")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "LogoutButton" })).toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "管理者メニュー" }))
      .toHaveAttribute("data-active-section", "payments");
    expect(screen.getByText("ページ内容")).toBeInTheDocument();
  });
});
