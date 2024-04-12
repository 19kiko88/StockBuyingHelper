import { HomeModule } from './home/home.module';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthModule } from './auth/auth.module';
import { FunctionsModule } from './functions/functions.module';
//import { AdminModule } from './admin/admin.module';


@NgModule({
  declarations: [],
  imports: [  
    CommonModule,
    HomeModule,
    AuthModule,
    FunctionsModule
    //AdminModule //改lazy loading
  ],
  exports:[
    HomeModule,  
    AuthModule,
    FunctionsModule
    //AdminModule //改lazy loading
  ]
})
export class PagesModule { }
