import { Route, Routes } from "react-router-dom";
import SignIn from "./pages/SignIn";
import ForgotPassword from "./pages/ForgotPassword";
import AccessRestricted from "./pages/AccessRestricted";
import NotFound from "./pages/NotFound";

export default function App() {
  return (
    <Routes>
      <Route index element={<SignIn />} />
      <Route path="forgot-password" element={<ForgotPassword />} />
      <Route path="access-restricted" element={<AccessRestricted />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}
