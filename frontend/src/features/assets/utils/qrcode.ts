import QRCode from "qrcode";

export async function generateQrDataUrl(
  text: string,
  size = 220
): Promise<string> {
  return QRCode.toDataURL(text, {
    width: size,
    margin: 4,
    errorCorrectionLevel: "M",
  });
}