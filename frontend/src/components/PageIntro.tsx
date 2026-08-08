interface PageIntroProps {
  kicker: string;
  title: string;
  lede?: string;
}

export default function PageIntro({ kicker, title, lede }: PageIntroProps) {
  return (
    <section className="page-intro">
      <div className="wrap">
        <p className="section-kicker">{kicker}</p>
        <h1 className="page-intro-title">{title}</h1>
        {lede && <p className="section-lede page-intro-lede">{lede}</p>}
      </div>
    </section>
  );
}
