import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { JwtInfoService } from '../services/jwt-info.service';

export const authGuard: CanActivateFn = (route, state) => 
{  

  const _router =  inject(Router);
  const _jwtService =  inject(JwtInfoService);

  if (_jwtService.jwt)
  {
    if (_jwtService.jwtExpired)
    {
      window.alert('jwt expired!');
      return _router.createUrlTree(['/login'])
    }
  }
  else
  {
    window.alert('no jwt!');
    return _router.createUrlTree(['/login'])
  }

  return true;
};
