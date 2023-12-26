import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MainComponent } from './modules/pages/home/components/main/main.component'
import { TestPageComponent } from './modules/pages/home/components/test-page/test-page.component';

const routes: Routes = [
  {path: '', component: MainComponent },
  {path: 'main', component: MainComponent },  
  {path: 'testpage', component: TestPageComponent },  
  {path: '**', redirectTo: 'main' }//沒有比對到路由
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
