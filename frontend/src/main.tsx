import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { ThunderIDProvider } from "@thunderid/react";
import App from "./App";
import "./styles/index.scss";

createRoot(document.getElementById("root") as HTMLElement).render(
  <StrictMode>
    <ThunderIDProvider
      baseUrl={import.meta.env.VITE_THUNDERID_BASE_URL || undefined}
      clientId={import.meta.env.VITE_THUNDERID_CLIENT_ID || undefined}
    >
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </ThunderIDProvider>
  </StrictMode>
);
