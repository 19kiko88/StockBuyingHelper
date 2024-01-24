import { HomeModule } from './home/home.module';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UsersModule } from './users/users.module';



@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    HomeModule,
    UsersModule
  ],
  exports:[
    HomeModule,
    UsersModule
  ]
})
export class PagesModule { }
