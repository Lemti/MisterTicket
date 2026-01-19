import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Récupérer le token depuis localStorage
  const token = typeof window !== 'undefined' && typeof localStorage !== 'undefined' 
    ? localStorage.getItem('token') 
    : null;

  // Si le token existe, cloner la requête et ajouter le header Authorization
  if (token) {
    const clonedReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    return next(clonedReq);
  }

  // Sinon, laisser passer la requête normale
  return next(req);
};