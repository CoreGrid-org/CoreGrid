import { Route, Routes } from "react-router-dom";
import Dashboard from "./pages/Dashboard";
import SignIn from "./pages/SignIn";
import Setup from "./pages/Setup";
import ForgotPassword from "./pages/ForgotPassword";
import AccessRestricted from "./pages/AccessRestricted";
import NotFound from "./pages/NotFound";

export default function App() {
  return (
    <Routes>
      <Route index element={<Dashboard />} />
      <Route path="signin" element={<SignIn />} />
      <Route path="setup" element={<Setup />} />
      <Route path="forgot-password" element={<ForgotPassword />} />
      <Route path="access-restricted" element={<AccessRestricted />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}
