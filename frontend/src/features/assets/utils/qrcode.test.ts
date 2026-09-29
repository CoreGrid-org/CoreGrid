import { describe, expect, it, vi } from "vitest";
import { generateQrDataUrl, downloadPrintableLabel } from "./qrcode";

vi.mock("qrcode", () => ({
  default: {
    toDataURL: vi.fn().mockResolvedValue("data:image/png;base64,mockqrcode"),
  },
}));

describe("qrcode utility", () => {
  it("generateQrDataUrl generates valid data url", async () => {
    const dataUrl = await generateQrDataUrl("COREGRID-ASSET-001");
    expect(dataUrl).toBe("data:image/png;base64,mockqrcode");
  });

  it("downloadPrintableLabel creates canvas and triggers download link click", async () => {
    // Mock canvas context and image
    const mockContext = {
      fillStyle: "",
      fillRect: vi.fn(),
      strokeStyle: "",
      lineWidth: 0,
      strokeRect: vi.fn(),
      drawImage: vi.fn(),
      font: "",
      fillText: vi.fn(),
    };

    const createElementOriginal = document.createElement.bind(document);
    const mockClick = vi.fn();

    vi.spyOn(document, "createElement").mockImplementation((tagName: string) => {
      if (tagName.toLowerCase() === "canvas") {
        const canvas = createElementOriginal("canvas");
        canvas.getContext = vi.fn().mockReturnValue(mockContext);
        canvas.toDataURL = vi.fn().mockReturnValue("data:image/png;base64,mockcanvas");
        return canvas;
      }
      if (tagName.toLowerCase() === "a") {
        const link = createElementOriginal("a");
        link.click = mockClick;
        return link;
      }
      return createElementOriginal(tagName);
    });

    // Mock Image constructor onload
    global.Image = class {
      onload: () => void = () => {};
      _src = "";
      set src(val: string) {
        this._src = val;
        setTimeout(() => this.onload(), 10);
      }
      get src() {
        return this._src;
      }
    } as unknown as typeof Image;

    await downloadPrintableLabel("AST-001", "Dell XPS Laptop with very long name that exceeds 35 characters limit", "QR-PAYLOAD-001");

    expect(mockContext.fillRect).toHaveBeenCalled();
    expect(mockContext.fillText).toHaveBeenCalledWith("AST-001", 190, 90, 290);
    expect(mockClick).toHaveBeenCalled();
  });
});
