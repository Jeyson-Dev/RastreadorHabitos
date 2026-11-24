// Superficie base: borde fino, radio de 12 px y sin sombra marcada.
export default function Tarjeta({ como: Elemento = 'section', className = '', children, ...resto }) {
  return (
    <Elemento className={`rounded-xl border border-borde bg-superficie p-5 ${className}`} {...resto}>
      {children}
    </Elemento>
  );
}
