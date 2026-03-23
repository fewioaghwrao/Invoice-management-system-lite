import { render, screen } from "@testing-library/react";
import CurrentUserBadge from "@/components/CurrentUserBadge";
import { useCurrentUser } from "@/hooks/useCurrentUser";

jest.mock("@/hooks/useCurrentUser", () => ({
  useCurrentUser: jest.fn(),
}));

const mockUseCurrentUser = useCurrentUser as jest.MockedFunction<
  typeof useCurrentUser
>;

describe("CurrentUserBadge", () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  it("user が null の場合は何も表示しない", () => {
    mockUseCurrentUser.mockReturnValue(null);

    const { container } = render(<CurrentUserBadge />);

    expect(container.firstChild).toBeNull();
  });

  it("Admin の場合は 管理者 と名前を表示する", () => {
    mockUseCurrentUser.mockReturnValue({
      id: 1,
      email: "admin@example.com",
      name: "管理者 太郎",
      role: "Admin",
      token: "dummy-token",
    });

    render(<CurrentUserBadge />);

    expect(screen.getByText("ロール: 管理者")).toBeInTheDocument();
    expect(screen.getByText("管理者 太郎")).toBeInTheDocument();
  });

  it("Member の場合は 一般会員 と名前を表示する", () => {
    mockUseCurrentUser.mockReturnValue({
      id: 2,
      email: "member@example.com",
      name: "会員 花子",
      role: "Member",
    });

    render(<CurrentUserBadge />);

    expect(screen.getByText("ロール: 一般会員")).toBeInTheDocument();
    expect(screen.getByText("会員 花子")).toBeInTheDocument();
  });
});