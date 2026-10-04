import { useEffect, useMemo, useState } from "react";
import { Button, FileUploaderDropContainer } from "@carbon/react";
import { Close, Image as ImageIcon } from "@carbon/icons-react";

// Matches PhotoUploadValidator.MaxSizeBytes on the backend.
const PHOTO_MAX_BYTES = 5 * 1024 * 1024;

interface MaintenancePhotoFieldProps {
  file: File | null;
  onChange: (file: File | null) => void;
  disabled?: boolean;
}

// Optional photo picker for a maintenance form. It only holds the file
// locally and previews it. The upload to storage happens when the form is
// submitted, so a photo picked and then abandoned never reaches the bucket.
export default function MaintenancePhotoField({ file, onChange, disabled }: MaintenancePhotoFieldProps) {
  const [error, setError] = useState<string | null>(null);

  // One object URL per picked file, freed when it's replaced or the form closes.
  const preview = useMemo(() => (file ? URL.createObjectURL(file) : null), [file]);
  useEffect(() => {
    return () => {
      if (preview) URL.revokeObjectURL(preview);
    };
  }, [preview]);

  const handleAdded = (added: File | undefined) => {
    if (!added) return;
    if (added.size > PHOTO_MAX_BYTES) {
      setError("That photo is larger than 5 MB. Choose a smaller one.");
      return;
    }
    setError(null);
    onChange(added);
  };

  return (
    <div className="cg-fault-modal__field">
      <p className="cds--label">
        Photo <span className="cg-fault-modal__optional">(optional)</span>
      </p>
      {file && preview ? (
        <div className="cg-fault-modal__photo">
          <img src={preview} alt="Attached photo" />
          <div className="cg-fault-modal__photo-meta">
            <p className="cg-fault-modal__photo-name">{file.name}</p>
            <p className="cg-fault-modal__photo-size">{(file.size / 1024 / 1024).toFixed(2)} MB</p>
            <p className="cg-fault-modal__photo-size">Uploaded when you submit.</p>
          </div>
          <Button
            kind="ghost"
            size="sm"
            hasIconOnly
            renderIcon={Close}
            iconDescription="Remove photo"
            tooltipPosition="left"
            onClick={() => onChange(null)}
            disabled={disabled}
          />
        </div>
      ) : (
        <>
          <div className="cg-fault-modal__drop">
            <ImageIcon size={24} />
            <FileUploaderDropContainer
              labelText="Drag a photo here or click to choose one"
              accept={[".jpg", ".jpeg", ".png", ".webp"]}
              multiple={false}
              disabled={disabled}
              onAddFiles={(_event, { addedFiles }) => handleAdded(addedFiles[0])}
            />
            <span className="cg-fault-modal__drop-hint">JPEG, PNG or WebP, up to 5 MB</span>
          </div>
          {error && <p className="cg-fault-modal__error">{error}</p>}
        </>
      )}
    </div>
  );
}
