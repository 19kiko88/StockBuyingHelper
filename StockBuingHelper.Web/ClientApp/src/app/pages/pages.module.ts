import { HomeModule } from './home/home.module';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoginModule } from './login/login.module';
import { FunctionsModule } from './functions/functions.module';
//import { AdminModule } from './admin/admin.module';


@NgModule({
  declarations: [],
  imports: [  
    CommonModule,
    HomeModule,
    LoginModule,
    FunctionsModule
    //AdminModule //改lazy loading
  ],
  exports:[
    HomeModule,  
    LoginModule,
    FunctionsModule
    //AdminModule //改lazy loading
  ]
})
export class PagesModule { }
