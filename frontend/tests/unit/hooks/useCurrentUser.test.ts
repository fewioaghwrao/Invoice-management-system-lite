import { renderHook, waitFor } from "@testing-library/react";
import { useCurrentUser } from "@/hooks/useCurrentUser";

describe("useCurrentUser", () => {
  beforeEach(() => {
    localStorage.clear();
    jest.restoreAllMocks();
  });

  it("localStorage に currentUser がなければ null を返す", async () => {
    const { result } = renderHook(() => useCurrentUser());

    await waitFor(() => {
      expect(result.current).toBeNull();
    });
  });

  it("正常な currentUser があれば user を返す", async () => {
    const user = {
      id: 1,
      email: "admin@example.com",
      name: "管理者 太郎",
      role: "Admin" as const,
      token: "dummy-token",
    };

    localStorage.setItem("currentUser", JSON.stringify(user));

    const { result } = renderHook(() => useCurrentUser());

    await waitFor(() => {
      expect(result.current).toEqual(user);
    });
  });

  it("壊れた JSON の場合は currentUser を削除し null のままにする", async () => {
    const removeItemSpy = jest.spyOn(Storage.prototype, "removeItem");
    localStorage.setItem("currentUser", "{broken json}");

    const { result } = renderHook(() => useCurrentUser());

    await waitFor(() => {
      expect(removeItemSpy).toHaveBeenCalledWith("currentUser");
      expect(result.current).toBeNull();
    });
  });

it("role が Member の currentUser も返す", async () => {
  const user = {
    id: 2,
    email: "member@example.com",
    name: "一般 会員",
    role: "Member" as const,
    token: "member-token",
  };

  localStorage.setItem("currentUser", JSON.stringify(user));

  const { result } = renderHook(() => useCurrentUser());

  await waitFor(() => {
    expect(result.current).toEqual(user);
  });
});

it("token がない currentUser も返す", async () => {
  const user = {
    id: 3,
    email: "member2@example.com",
    name: "会員 次郎",
    role: "Member" as const,
  };

  localStorage.setItem("currentUser", JSON.stringify(user));

  const { result } = renderHook(() => useCurrentUser());

  await waitFor(() => {
    expect(result.current).toEqual(user);
  });
});

it("JSON として読めるデータなら shape 不足でもそのまま返す", async () => {
  const rawUser = {
    email: "test@example.com",
  };

  localStorage.setItem("currentUser", JSON.stringify(rawUser));

  const { result } = renderHook(() => useCurrentUser());

  await waitFor(() => {
    expect(result.current).toEqual(rawUser);
  });
});

});