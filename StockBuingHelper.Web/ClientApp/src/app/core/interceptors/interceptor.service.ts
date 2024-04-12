import { Injectable } from '@angular/core';
import { JwtInfoService } from '../services/jwt-info.service';
import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})

export class InterceptorService implements HttpInterceptor
{

  constructor(
    private _jwtInfoService: JwtInfoService
  ) { }


  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>>
  {
    const jwt = this._jwtInfoService.jwt;

    if(jwt)
    {
      req = req.clone({
        headers: req.headers.set('Authorization', `bearer ${jwt}`)
      })
    }

    //return next.handle(req);

    return next.handle(req).pipe(  
      catchError((err: HttpErrorResponse) => {

          if(err instanceof HttpErrorResponse)
          {
            switch (err.status) 
            {
              case 500:
                window.alert(`An error occurred：內部程式錯誤，請聯繫維修人員。`);
                break;
              default:
                window.alert(`An error occurred： Error Status [${err.status}] Error`);    
            }                      
          }

          return throwError(() => new Error(err.error));
        }
      )
    )
  } 

}
