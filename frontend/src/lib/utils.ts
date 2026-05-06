import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

// Helper estándar usado por componentes shadcn para combinar clases tailwind.
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
