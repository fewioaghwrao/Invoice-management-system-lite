import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import LogoutButton from "@/components/LogoutButton";

const pushMock = jest.fn();

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: pushMock,
  }),
}));

describe("LogoutButton", () => {
  const originalFetch = global.fetch;

  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();

    Object.defineProperty(global, "fetch", {
      writable: true,
      value: jest.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ ok: true }),
      }),
    });
  });

  afterEach(() => {
    global.fetch = originalFetch;
  });

  it("確認後に currentUser を削除し、logout API を呼び、ログイン画面へ遷移する", async () => {
    const user = userEvent.setup();
    const removeItemSpy = jest.spyOn(Storage.prototype, "removeItem");

    localStorage.setItem(
      "currentUser",
      JSON.stringify({
        id: 1,
        email: "admin@example.com",
        name: "管理者 太郎",
        role: "Admin",
        token: "dummy-token",
      })
    );

    render(<LogoutButton />);

    // 1回目：確認モーダルを開く
    await user.click(screen.getByRole("button", { name: "ログアウト" }));

    expect(screen.getByText("ログアウト確認")).toBeInTheDocument();
    expect(screen.getByText("本当にログアウトしますか？")).toBeInTheDocument();

    // 2回目：モーダル内のログアウトを押す
    const logoutButtons = screen.getAllByRole("button", { name: "ログアウト" });
    await user.click(logoutButtons[1]);

    await waitFor(() => {
      expect(removeItemSpy).toHaveBeenCalledWith("currentUser");
      expect(global.fetch).toHaveBeenCalledWith("/api/auth/logout", {
        method: "POST",
      });
      expect(pushMock).toHaveBeenCalledWith("/auth/login");
    });
  });

  it("確認後にログイン関連 cookie を期限切れで削除する", async () => {
    const user = userEvent.setup();

    const cookieSetter = jest.fn();
    Object.defineProperty(document, "cookie", {
      configurable: true,
      set: cookieSetter,
    });

    render(<LogoutButton />);

    // 1回目：確認モーダルを開く
    await user.click(screen.getByRole("button", { name: "ログアウト" }));

    expect(screen.getByText("ログアウト確認")).toBeInTheDocument();

    // 2回目：モーダル内のログアウトを押す
    const logoutButtons = screen.getAllByRole("button", { name: "ログアウト" });
    await user.click(logoutButtons[1]);

    await waitFor(() => {
      expect(cookieSetter).toHaveBeenCalledWith(
        "isLoggedIn=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT"
      );
      expect(cookieSetter).toHaveBeenCalledWith(
        "role=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT"
      );
    });
  });

  it("キャンセルを押すとログアウト処理を実行しない", async () => {
    const user = userEvent.setup();
    const removeItemSpy = jest.spyOn(Storage.prototype, "removeItem");

    render(<LogoutButton />);

    await user.click(screen.getByRole("button", { name: "ログアウト" }));

    expect(screen.getByText("ログアウト確認")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "キャンセル" }));

    await waitFor(() => {
      expect(screen.queryByText("ログアウト確認")).not.toBeInTheDocument();
    });

    expect(removeItemSpy).not.toHaveBeenCalledWith("currentUser");
    expect(global.fetch).not.toHaveBeenCalled();
    expect(pushMock).not.toHaveBeenCalled();
  });
});