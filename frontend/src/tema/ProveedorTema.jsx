import { useCallback, useEffect, useMemo, useState } from 'react';
import { CLAVE_TEMA, ContextoTema, PREFERENCIAS_TEMA } from './contextoTema';

const CONSULTA_OSCURO = '(prefers-color-scheme: dark)';

function leerPreferencia() {
  try {
    const guardada = localStorage.getItem(CLAVE_TEMA);
    return PREFERENCIAS_TEMA.includes(guardada) ? guardada : 'sistema';
  } catch {
    return 'sistema';
  }
}

// Aplica la clase .dark en <html> según la preferencia; con «sistema» sigue los cambios del equipo.
export default function ProveedorTema({ children }) {
  const [preferencia, setPreferencia] = useState(leerPreferencia);
  const [sistemaOscuro, setSistemaOscuro] = useState(() => window.matchMedia(CONSULTA_OSCURO).matches);

  useEffect(() => {
    const consulta = window.matchMedia(CONSULTA_OSCURO);
    const alCambiar = (evento) => setSistemaOscuro(evento.matches);
    consulta.addEventListener('change', alCambiar);
    return () => consulta.removeEventListener('change', alCambiar);
  }, []);

  const oscuro = preferencia === 'oscuro' || (preferencia === 'sistema' && sistemaOscuro);

  useEffect(() => {
    document.documentElement.classList.toggle('dark', oscuro);
  }, [oscuro]);

  const cambiarPreferencia = useCallback((nueva) => {
    setPreferencia(nueva);
    try {
      localStorage.setItem(CLAVE_TEMA, nueva);
    } catch {
      // Sin almacenamiento (modo privado estricto): el tema vale solo para esta visita.
    }
  }, []);

  const valor = useMemo(() => ({ preferencia, oscuro, cambiarPreferencia }), [preferencia, oscuro, cambiarPreferencia]);

  return <ContextoTema.Provider value={valor}>{children}</ContextoTema.Provider>;
}
