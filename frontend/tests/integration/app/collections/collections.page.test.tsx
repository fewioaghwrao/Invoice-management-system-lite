import { render, screen } from "@testing-library/react";
import CollectionsPage from "../../../../src/app/collections/[invoiceId]/page";

const mockCollectionsClient = jest.fn();

jest.mock(
  "../../../../src/app/collections/[invoiceId]/CollectionsClient",
  () => {
    return function MockCollectionsClient(props: { invoiceId: string }) {
      mockCollectionsClient(props);
      return <div>CollectionsClient</div>;
    };
  }
);

describe("CollectionsPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("CollectionsClient を表示する", async () => {
    const element = await CollectionsPage({
      params: Promise.resolve({ invoiceId: "INV-001" }),
    });

    render(element);

    expect(screen.getByText("CollectionsClient")).toBeInTheDocument();
  });

  it("params.invoiceId を CollectionsClient に渡す", async () => {
    const element = await CollectionsPage({
      params: Promise.resolve({ invoiceId: "INV-001" }),
    });

    render(element);

    expect(mockCollectionsClient).toHaveBeenCalledTimes(1);
    expect(mockCollectionsClient).toHaveBeenCalledWith({
      invoiceId: "INV-001",
    });
  });
});