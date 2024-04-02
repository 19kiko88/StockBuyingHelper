import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { JwtInfoService } from '../services/jwt-info.service';

export const authGuard: CanActivateFn = (route, state) => {  

  const _router =  inject(Router);
  const _jwtService =  inject(JwtInfoService);

  if (_jwtService.jwt)
  {
    if (_jwtService.jwtExpired)
    {
      console.log('jwt expired, login again.');
      _jwtService.setJwtValid(false);
      return _router.createUrlTree(['/login'])
    }
  }
  else
  {
    console.log('not login.');
    _jwtService.setJwtValid(false);
    return _router.createUrlTree(['/login'])
  }

  _jwtService.setJwtValid(true);

  return true;
};
