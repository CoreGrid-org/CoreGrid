import { useState } from "react";
import { Modal } from "@carbon/react";
import { Image as ImageIcon, Launch } from "@carbon/icons-react";

interface PhotoThumbnailProps {
  /** A signed storage link from the API (valid ~15 minutes) or an older public URL. */
  url: string;
  alt: string;
  /** Viewer title, e.g. "Fault photo: MOHSL-MED-MON-0002". */
  title: string;
  /** "sm" for table rows, "md" for detail views. */
  size?: "sm" | "md";
}

// A clickable photo thumbnail that opens the full image in a viewer. Photo
// links are short-lived signed URLs, so a page left open for a while shows a
// "reload to view" hint instead of a broken image.
export default function PhotoThumbnail({ url, alt, title, size = "sm" }: PhotoThumbnailProps) {
  const [open, setOpen] = useState(false);
  const [failed, setFailed] = useState(false);

  if (failed) {
    return (
      <span className={`cg-photo-thumb cg-photo-thumb--${size} is-failed`} title="The photo link has expired. Reload the page to view it.">
        <ImageIcon size={size === "sm" ? 16 : 24} />
        {size === "md" && <span>Photo link expired. Reload the page to view it.</span>}
      </span>
    );
  }

  return (
    <>
      <button type="button" className={`cg-photo-thumb cg-photo-thumb--${size}`} onClick={() => setOpen(true)} aria-label={`View photo: ${alt}`}>
        <img src={url} alt={alt} loading="lazy" onError={() => setFailed(true)} />
      </button>

      {open && (
        <Modal open passiveModal size="lg" modalLabel="Photo" modalHeading={title} onRequestClose={() => setOpen(false)}>
          <div className="cg-photo-viewer">
            <img src={url} alt={alt} onError={() => setFailed(true)} />
            <a href={url} target="_blank" rel="noreferrer" className="cg-photo-viewer__open">
              <Launch size={14} /> Open full size in a new tab
            </a>
          </div>
        </Modal>
      )}
    </>
  );
}
