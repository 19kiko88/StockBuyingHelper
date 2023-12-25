import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MainComponent } from './modules/home/pages/main/main.component';

const routes: Routes = [
  {path: '', component: MainComponent },
  {path: 'main', component: MainComponent },  
  {path: '**', redirectTo: 'main' }//沒有比對到路由
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
