import { CircleAlert, CircleCheck, Info, TriangleAlert } from 'lucide-react';

const TIPOS = {
  info: { icono: Info, clases: 'bg-info-suave text-info border-info/30' },
  exito: { icono: CircleCheck, clases: 'bg-acento-suave text-acento border-acento/30' },
  aviso: { icono: TriangleAlert, clases: 'bg-aviso-suave text-aviso border-aviso/30' },
  error: { icono: CircleAlert, clases: 'bg-peligro-suave text-peligro border-peligro/30' },
};

// Mensaje en línea. Los errores se anuncian de inmediato (role="alert"); el resto, con cortesía.
export default function Aviso({ tipo = 'info', titulo, children, className = '' }) {
  const { icono: Icono, clases } = TIPOS[tipo];
  return (
    <div
      role={tipo === 'error' ? 'alert' : 'status'}
      className={`flex gap-3 rounded-lg border px-4 py-3 text-sm ${clases} ${className}`}
    >
      <Icono className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
      <div className="flex flex-col gap-0.5">
        {titulo && <p className="font-semibold">{titulo}</p>}
        {children && <div className="text-texto/90">{children}</div>}
      </div>
    </div>
  );
}
