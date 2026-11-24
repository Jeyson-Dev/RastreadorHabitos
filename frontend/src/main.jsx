import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import '@fontsource-variable/inter';
import './estilos.css';

createRoot(document.getElementById('raiz')).render(
  <StrictMode>
    <main className="grid min-h-dvh place-items-center">
      <p className="text-suave">Rastreador de hábitos</p>
    </main>
  </StrictMode>,
);
