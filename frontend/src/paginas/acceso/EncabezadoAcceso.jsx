// Título y bajada comunes a las pantallas de acceso. Con «ref», el título puede recibir el foco
// (por ejemplo, al pasar de un paso a otro) para que el lector de pantalla lo anuncie.
export default function EncabezadoAcceso({ titulo, children, ref }) {
  return (
    <div className="mb-6 flex flex-col gap-1.5">
      <h1 ref={ref} tabIndex={ref ? -1 : undefined} className="text-2xl font-semibold tracking-tight outline-none">
        {titulo}
      </h1>
      {children && <p className="text-sm text-suave">{children}</p>}
    </div>
  );
}
