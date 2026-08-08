export default function PublicNotice() {
  return (
    <div className="public-notice">
      <span className="public-notice-tag">Public overview</span>
      <p>
        This site is a public, informational summary — no sign-in, personal data or asset records are
        exposed here. Authentication, per-organisation data and workflow actions belong to the
        authenticated CoreGrid application and are a separate, later piece of work.
      </p>
    </div>
  );
}
