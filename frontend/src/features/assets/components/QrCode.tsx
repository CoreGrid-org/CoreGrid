import { useEffect, useState } from "react";
import { generateQrDataUrl } from "../utils/qrcode";

interface QrCodeProps {
  value: string;
  size?: number;
}

export default function QrCode({ value, size = 180 }: QrCodeProps) {
  const [qrUrl, setQrUrl] = useState<string>("");

  useEffect(() => {
    let cancelled = false;
    generateQrDataUrl(value, size)
      .then((url) => {
        if (!cancelled) setQrUrl(url);
      })
      .catch(() => {
        if (!cancelled) setQrUrl("");
      });
    return () => {
      cancelled = true;
    };
  }, [value, size]);

  if (!qrUrl) {
    return (
      <div
        style={{
          width: size,
          height: size,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          border: "1px solid #e0e0e0",
        }}
      >
        Could not render QR code
      </div>
    );
  }

  return (
    <img
      src={qrUrl}
      width={size}
      height={size}
      alt={`QR code for ${value}`}
    />
  );
}