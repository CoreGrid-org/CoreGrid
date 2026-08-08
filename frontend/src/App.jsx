import Header from "./components/Header.jsx";
import Hero from "./components/Hero.jsx";
import Overview from "./components/Overview.jsx";
import Architecture from "./components/Architecture.jsx";
import Roles from "./components/Roles.jsx";
import AgenticAI from "./components/AgenticAI.jsx";
import Capabilities from "./components/Capabilities.jsx";
import Stack from "./components/Stack.jsx";
import Footer from "./components/Footer.jsx";

export default function App() {
  return (
    <>
      <div className="blueprint-bg" aria-hidden="true"></div>
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <Header />
      <main id="main">
        <Hero />
        <Overview />
        <Architecture />
        <Roles />
        <AgenticAI />
        <Capabilities />
        <Stack />
      </main>
      <Footer />
    </>
  );
}
