
import { Injectable } from '@angular/core';
import { JwtInfoService } from '../services/jwt-info.service';
import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, catchError, map, throwError } from 'rxjs';
import { IResultDto } from '../dtos/response/result-dto';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})

export class InterceptorService implements HttpInterceptor
{

  constructor(
    private _jwtInfoService: JwtInfoService,
    private _router:Router
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
      // map(event => {
      //   if (event instanceof HttpResponse){
      //   }
        
      //   return event;
      // }),
      catchError((err: HttpErrorResponse) => {

          if(err instanceof HttpErrorResponse)
          {
            if(err.error && typeof(err.error) == 'object')
            {
              window.alert(`[${err.error.Content}] Error - ${err.error.Message}`);                     
            }
            else 
            {
              window.alert(`[${err.status}] Error - 請聯繫系統管理員.`);       
              if(err.status == 401){
                this._router.navigate(['/login']);
              }
            }
          }

          return throwError(() => new Error(/*err.error*/));
        }
      )
    )
  } 

}
