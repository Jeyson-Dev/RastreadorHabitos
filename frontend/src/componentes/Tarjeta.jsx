// Superficie base de vidrio: translúcida, con desenfoque, radio de 20 px y sombra cálida.
export default function Tarjeta({ como: Elemento = 'section', className = '', children, ...resto }) {
  return (
    <Elemento className={`vidrio rounded-[1.25rem] p-5 ${className}`} {...resto}>
      {children}
    </Elemento>
  );
}
