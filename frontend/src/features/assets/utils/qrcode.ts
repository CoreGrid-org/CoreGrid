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

export async function downloadPrintableLabel(
  assetCode: string,
  assetName: string,
  qrPayload: string
): Promise<void> {
  const canvas = document.createElement("canvas");
  const ctx = canvas.getContext("2d");
  if (!ctx) return;

  canvas.width = 500;
  canvas.height = 200;

  // Background
  ctx.fillStyle = "#ffffff";
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  
  // Border
  ctx.strokeStyle = "#e0e0e0";
  ctx.lineWidth = 2;
  ctx.strokeRect(1, 1, canvas.width - 2, canvas.height - 2);

  const qrDataUrl = await generateQrDataUrl(qrPayload, 160);
  
  const img = new Image();
  img.src = qrDataUrl;
  
  await new Promise((resolve) => {
    img.onload = () => {
      ctx.drawImage(img, 20, 20, 160, 160);
      
      ctx.fillStyle = "#161616";
      ctx.font = "bold 24px monospace";
      ctx.fillText(assetCode, 190, 90, 290);
      
      ctx.font = "18px sans-serif";
      ctx.fillStyle = "#525252";
      let nameToDraw = assetName;
      if (nameToDraw.length > 35) {
        nameToDraw = nameToDraw.substring(0, 35) + "...";
      }
      ctx.fillText(nameToDraw, 190, 140, 290);
      
      resolve(true);
    };
  });

  const link = document.createElement("a");
  link.download = `asset-label-${assetCode}.png`;
  link.href = canvas.toDataURL("image/png");
  link.click();
}