import { Tag, Dropdown, Link } from "@carbon/react";
import { LogoGithub } from "@carbon/icons-react";

const REPO_URL = "https://github.com/CoreGrid-org/CoreGrid";

export default function AboutPanel() {
  return (
    <>
      <div className="cg-section">
        <div className="cg-section__header">
          <p className="cg-section__title">Language</p>
        </div>
        <div className="cg-section__body">
          <Dropdown
            id="settings-language"
            titleText="Display language"
            helperText="CoreGrid is currently available in English only."
            label="English (US)"
            items={["English (US)"]}
            selectedItem="English (US)"
            disabled
            onChange={() => {}}
          />
        </div>
      </div>

      <div className="cg-section cg-section--spaced">
        <div className="cg-section__header">
          <p className="cg-section__title">About CoreGrid</p>
        </div>
        <div className="cg-section__body cg-about-list">
          <div className="cg-about-list__row">
            <p className="cg-about-list__label">Version</p>
            <Tag type="high-contrast" size="lg">{`v${__APP_VERSION__}`}</Tag>
          </div>
          <div className="cg-about-list__row">
            <p className="cg-about-list__label">Source code</p>
            <Link href={REPO_URL} target="_blank" rel="noreferrer" renderIcon={LogoGithub}>
              CoreGrid-org/CoreGrid
            </Link>
          </div>
        </div>
      </div>
    </>
  );
}
