import { Route, Routes } from "react-router-dom";
import Layout from "./layouts/Layout";
import Home from "./pages/Home";
import Platform from "./pages/Platform";
import RolesPage from "./pages/RolesPage";
import AgenticAIPage from "./pages/AgenticAIPage";
import About from "./pages/About";

export default function App() {
  return (
    <>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<Home />} />
          <Route path="platform" element={<Platform />} />
          <Route path="roles" element={<RolesPage />} />
          <Route path="agentic-ai" element={<AgenticAIPage />} />
          <Route path="about" element={<About />} />
        </Route>
      </Routes>
    </>
  );
}
