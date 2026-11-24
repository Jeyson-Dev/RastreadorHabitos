// Título y bajada comunes a las pantallas de acceso.
export default function EncabezadoAcceso({ titulo, children }) {
  return (
    <div className="mb-6 flex flex-col gap-1.5">
      <h1 className="text-2xl font-semibold tracking-tight">{titulo}</h1>
      {children && <p className="text-sm text-suave">{children}</p>}
    </div>
  );
}
