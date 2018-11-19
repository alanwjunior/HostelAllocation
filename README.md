## HostelAllocation
Esse projeto foi desenvolvido Projeto de Conclusão de Curso de Engenharia de Sistemas. Trata-se da otimização da alocação de quartos de um hostel. O projeto é dividido em uma interface web desenvolvida em VueJS e um backend responsável pela otimização de instâncias de testes através da utilização da biblioteca de otimização Gurobi.

### Pré-requisitos
Para execução do software é necessário possuir instalado o .Net Core, NodeJS e uma licensa válida para utilizar a biblioteca de otimização Gurobi.

### Running
A pasta client contém a interface web desenvolvida para simulação e criação de instâncias de teste. Primeiramente é necessário instalar os módulos para execução da aplicação:
```
npm install
```
Finalizada essa etapa, basta executar a aplicação:
```
npm run serve
```
O back-end se encontra na pasta HostelAllocationOptimization e basta compilar o projeto para execução.
